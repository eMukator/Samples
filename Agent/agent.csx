#!/usr/bin/env dotnet-script
// Jednoduchý agent pro lokální model s OpenAI-kompatibilním API.
// Spuštění: dotnet script agent.csx
// Příkazy: /new = nová konverzace, /exit = konec

using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

// ---------- Konfigurace (přes proměnné prostředí) ----------
var baseUrl = Environment.GetEnvironmentVariable("AGENT_BASE_URL") ?? "http://localhost:12434/engines/v1";
var model   = Environment.GetEnvironmentVariable("AGENT_MODEL")    ?? "ai/llama3.2";
var apiKey  = Environment.GetEnvironmentVariable("AGENT_API_KEY");
var root    = Path.GetFullPath(Environment.GetEnvironmentVariable("AGENT_ROOT") ?? Directory.GetCurrentDirectory());
const int MaxToolRounds = 10;
const int MaxReadChars = 100_000;

// Bez tohoto Windows konzole čte/píše diakritiku rozbitě (máš -> m├í┼í).
Console.InputEncoding = Console.OutputEncoding = new UTF8Encoding(false);

// Agent je jen anglický: na přepínání jazyků je llama3.2 moc slabá.
var systemPrompt = "You are a helpful assistant. Be brief and factual. Always reply in English.";

// Nástroje popisujeme v promptu a volání parsujeme sami. Nativní "tools" nejdou: llama3.2 píše
// volání i ve tvaru {"type":"function","function":"x",...}, který server (llama.cpp) neumí
// rozparsovat a vrátí HTTP 500 — pro některé dotazy pokaždé.
string ToolPrompt(bool canWrite) =>
    "\n\nYou can work with text files in the working directory using these tools:\n" +
    "- list_files(path): list files and folders in a directory, path \".\" is the working directory\n" +
    "- read_file(path): return the content of a text file\n" +
    (canWrite ? "- write_file(path, content): write text to a file (overwrites it)\n" : "") +
    "\nTo use a tool, reply with ONLY a JSON object and nothing else, e.g.:\n" +
    "{\"name\": \"read_file\", \"parameters\": {\"path\": \"demo1.txt\"}}\n" +
    "You may put several JSON objects on separate lines. You then get the results and can call more tools or answer.\n" +
    "Rules:\n" +
    "- NEVER guess file names or file contents. To know them, call a tool.\n" +
    "- Use relative paths, e.g. \"demo1.txt\" or \"folder/file.txt\".\n" +
    (canWrite ? "- Read the files you need BEFORE writing. Write the real content, never a placeholder.\n" : "") +
    "- Never call the same tool with the same arguments twice.\n" +
    "- When you have what you need, answer the user in plain text (no JSON), using the real results.";

var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
if (!string.IsNullOrEmpty(apiKey))
    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

// ---------- Implementace nástrojů ----------
string SafePath(string rel)
{
    var full = Path.GetFullPath(Path.Combine(root, string.IsNullOrWhiteSpace(rel) ? "." : rel));
    var rootSep = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
    if (full != root && !full.StartsWith(rootSep, StringComparison.OrdinalIgnoreCase))
        throw new Exception("Cesta je mimo pracovní adresář.");
    return full;
}

string Rel(string full) => Path.GetRelativePath(root, full).Replace('\\', '/');

string ExecuteTool(string name, JsonNode args)
{
    try
    {
        var path = args?["path"]?.ToString();
        switch (name)
        {
            case "list_files":
            {
                var dir = SafePath(path ?? ".");
                if (!Directory.Exists(dir)) return "Chyba: adresář neexistuje.";
                var entries = Directory.GetDirectories(dir).Select(d => Rel(d) + "/")
                    .Concat(Directory.GetFiles(dir).Select(Rel));
                var list = string.Join("\n", entries);
                return list.Length == 0 ? "(prázdný adresář)" : list;
            }
            case "read_file":
            {
                var file = SafePath(path ?? throw new Exception("Chybí parametr path."));
                // "demo2" -> "demo2.txt", pokud je kandidát jediný; jinak model po chybě bloudí po adresáři.
                var parent = Path.GetDirectoryName(file);
                if (!File.Exists(file) && Path.GetExtension(file) == "" && Directory.Exists(parent)
                    && Directory.GetFiles(parent, Path.GetFileName(file) + ".*") is { Length: 1 } only)
                    file = only[0];
                if (!File.Exists(file)) return "Chyba: soubor neexistuje.";
                var text = File.ReadAllText(file, Encoding.UTF8);
                return text.Length > MaxReadChars
                    ? text.Substring(0, MaxReadChars) + "\n...[zkráceno]"
                    : text;
            }
            case "write_file":
            {
                var file = SafePath(path ?? throw new Exception("Chybí parametr path."));
                var content = args?["content"]?.ToString() ?? "";
                // Malý model rád zapisuje i tam, kde nemá — zápis vždy potvrzuje uživatel.
                var shown = content.Length > 500 ? content.Substring(0, 500) + "\n...[zkráceno]" : content;
                Console.Write($"[potvrzení] Zapsat {content.Length} znaků do {Rel(file)}:\n---\n{shown}\n---\nPovolit? [a/N] ");
                if (Console.ReadLine()?.Trim().ToLowerInvariant() is not ("a" or "ano" or "y"))
                    return "The user rejected the write. Do not try again, answer the user.";
                var dir = Path.GetDirectoryName(file);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(file, content, new UTF8Encoding(false));
                return $"Zapsáno {content.Length} znaků do {Rel(file)}.";
            }
            default:
                return $"Chyba: neznámý nástroj '{name}'.";
        }
    }
    catch (Exception ex)
    {
        return "Chyba: " + ex.Message;
    }
}

// ---------- Parsování volání nástrojů z textu ----------
// Najde v textu všechny top-level JSON objekty (vyvážené závorky, respektuje řetězce).
IEnumerable<JsonObject> JsonObjects(string s)
{
    for (int i = 0; i < s.Length; i++)
    {
        if (s[i] != '{') continue;
        int depth = 0;
        bool inStr = false;
        for (int j = i; j < s.Length; j++)
        {
            char c = s[j];
            if (inStr) { if (c == '\\') j++; else if (c == '"') inStr = false; continue; }
            if (c == '"') inStr = true;
            else if (c == '{') depth++;
            else if (c == '}' && --depth == 0)
            {
                JsonNode node = null;
                try { node = JsonNode.Parse(s.Substring(i, j - i + 1)); } catch { }
                if (node is JsonObject obj) { yield return obj; i = j; }
                break;
            }
        }
    }
}

JsonNode ParseArgs(JsonNode argNode)
{
    // Argumenty chodí jako objekt, občas jako string s JSONem.
    if (argNode is JsonValue v && v.TryGetValue<string>(out var s))
    {
        if (string.IsNullOrWhiteSpace(s)) return new JsonObject();
        try { return JsonNode.Parse(s); } catch { return new JsonObject(); }
    }
    return argNode ?? new JsonObject();
}

var toolNames = new[] { "list_files", "read_file", "write_file" };

// Llama3.2 píše {"name":"x","parameters":{..}} i {"type":"function","function":"x","parameters":{..}},
// případně OpenAI tvar {"function":{"name":"x","arguments":".."}}. Bereme všechny.
List<(string Name, JsonNode Args)> ToolCalls(string content)
{
    var calls = new List<(string, JsonNode)>();
    foreach (var o in JsonObjects(content ?? ""))
    {
        var fn = o["function"];
        var name = o["name"]?.ToString()
                   ?? (fn is JsonObject fo ? fo["name"]?.ToString() : fn?.ToString());
        if (!toolNames.Contains(name)) continue;
        calls.Add((name, ParseArgs(o["parameters"] ?? o["arguments"] ?? (fn as JsonObject)?["arguments"])));
    }
    return calls;
}

// ---------- Volání modelu ----------
async Task<string> Chat(JsonArray messages)
{
    var body = new JsonObject
    {
        ["model"] = model,
        ["messages"] = JsonNode.Parse(messages.ToJsonString()),
        ["temperature"] = 0.2, // nižší náhodnost = méně výmyslů a bloudění
        ["stream"] = false
    };
    using var req = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
    using var resp = await http.PostAsync(baseUrl.TrimEnd('/') + "/chat/completions", req);
    var text = await resp.Content.ReadAsStringAsync();
    if (!resp.IsSuccessStatusCode)
        throw new Exception($"HTTP {(int)resp.StatusCode}: {text}");

    return JsonNode.Parse(text)?["choices"]?[0]?["message"]?["content"]?.ToString()
           ?? throw new Exception("Neočekávaná odpověď: " + text);
}

// Llama 3.2 s nabídnutými nástroji volá nástroje skoro vždy (i na "Ahoj") a na klasifikaci
// ANO/NE je moc slabá. Nástroje proto nabízíme jen když zpráva zmiňuje soubory nebo název souboru.
// ponytail: heuristika na klíčová slova, u silnějšího modelu (qwen, llama 8B+) lze vyhodit a nabízet vždy.
var fileIntent = new Regex(
    @"\bfiles?\b|folder|director|\bread\b|\bwrite\b|\bsave\b|\blist\b|content|\w\.[a-z]\w{0,4}\b",
    RegexOptions.IgnoreCase);

// write_file jen při výslovném požadavku na zápis: jinak ho model použije i na "combine ... and show me".
var writeIntent = new Regex(@"\bwrite\b|\bsave\b|\bcreate\b|overwrite|\bstore\b", RegexOptions.IgnoreCase);

// ---------- Konverzace ----------
JsonArray NewConversation() => new JsonArray
{
    new JsonObject { ["role"] = "system", ["content"] = systemPrompt }
};

var messages = NewConversation();

JsonArray CopyOf(JsonArray array) => JsonNode.Parse(array.ToJsonString())!.AsArray();

// Přidá odpověď modelu a naši reakci na ni.
void AddExchange(JsonArray work, string reply, string userContent)
{
    work.Add(new JsonObject { ["role"] = "assistant", ["content"] = reply });
    work.Add(new JsonObject { ["role"] = "user", ["content"] = userContent });
}

// Důvod, proč volání odmítnout; null = volání lze provést.
string RejectReason(string name, JsonNode args, bool canWrite)
{
    if (name != "write_file") return null;
    if (!canWrite)
        return "write_file is not available: the user did not ask to write a file.";
    // Model neumí "přečti a zapiš" naplánovat a vnoří čtení do zápisu (content: "read_file").
    if (args["parameters"] != null || toolNames.Contains(args["content"]?.ToString()))
        return "Invalid write: content must be the real text. First call read_file for the files you need, " +
               "then in your NEXT reply call write_file with the actual text you read.";
    return null;
}

// Provede volání jednoho kola. Vrací výsledky kola pro model, null = jen opakovaná volání (model se cyklí).
string RunRound(List<(string Name, JsonNode Args)> calls, bool canWrite, HashSet<string> seen, StringBuilder results)
{
    var roundResults = new StringBuilder();
    bool anyNew = false;
    foreach (var (name, args) in calls)
    {
        var argsJson = args.ToJsonString();
        Console.WriteLine($"[nástroj] {name} {argsJson}");

        string result;
        if (!seen.Add(name + argsJson))
            result = "This call was already made, its result is above. Do not repeat it.";
        else
        {
            anyNew = true;
            result = RejectReason(name, args, canWrite);
            if (result == null)
            {
                result = ExecuteTool(name, args);
                results.Append($"{name} {argsJson}:\n{result}\n\n");
            }
        }
        var preview = result.Length > 200 ? result.Substring(0, 200) + "..." : result;
        Console.WriteLine($"[výsledek] {preview.Replace("\n", " | ")}");
        roundResults.Append($"{name} {argsJson}:\n{result}\n\n");
    }
    return anyNew ? roundResults.ToString() : null;
}

// Odpověď bez nástrojů, s dosavadními výsledky jako textem (po limitu kol nebo cyklení).
async Task<string> ForcedAnswer(string input, StringBuilder results)
{
    var final = CopyOf(messages);
    final[final.Count - 1] = new JsonObject
    {
        ["role"] = "user",
        ["content"] = $"{input}\n\n[Tool results]\n{results}" +
                      "Answer my request above using these results. Do not make anything up."
    };
    return await Chat(final);
}

// Práce s nástroji běží nad pracovní kopií; do historie jdou jen dotaz + finální odpověď,
// jinak model v dalších kolech napodobuje formát výsledků a vymýšlí si falešné.
async Task<string> AnswerWithTools(string input)
{
    bool canWrite = writeIntent.IsMatch(input);
    var work = CopyOf(messages);
    work[0] = new JsonObject { ["role"] = "system", ["content"] = systemPrompt + ToolPrompt(canWrite) };
    var seen = new HashSet<string>();
    var results = new StringBuilder();
    bool nudged = false;
    string answer = null;

    for (int round = 0; round < MaxToolRounds; round++)
    {
        var reply = await Chat(work);
        var calls = ToolCalls(reply);
        if (calls.Count == 0 && results.Length == 0 && !nudged)
        {
            // Model občas obsah souboru rovnou vymyslí; jednou ho postrčíme k nástroji.
            nudged = true;
            AddExchange(work, reply,
                "You did not use any tool. Never guess file names or contents. If my request needs " +
                "file data, reply now with ONLY the JSON tool call. Otherwise repeat your answer.");
            continue;
        }
        if (calls.Count == 0) { answer = reply; break; }

        var roundResults = RunRound(calls, canWrite, seen, results);
        if (roundResults == null) break;
        AddExchange(work, reply, $"[Tool results]\n{roundResults}" +
            "Call more tools only if you still need something. Otherwise answer my request " +
            $"\"{input}\" in plain text, using these results.");
    }

    answer ??= await ForcedAnswer(input, results);

    // Víc kroků (přečti -> zapiš) llama3.2 nedotáhne a pak odpoví, jako by bylo hotovo.
    if (canWrite && !results.ToString().Contains("Zapsáno "))
        Console.WriteLine("\n[pozor] Požadovaný zápis neproběhl, žádný soubor se nezměnil.");
    return answer;
}

Console.WriteLine($"Model: {model} @ {baseUrl}");
Console.WriteLine($"Pracovní adresář: {root}");
Console.WriteLine("Příkazy: /new = nová konverzace, /exit = konec");

while (true)
{
    Console.Write("\nTy> ");
    var input = Console.ReadLine()?.Trim();
    if (input is null or "/exit" or "/quit") break;
    if (input.Length == 0) continue;
    if (input == "/new")
    {
        messages = NewConversation();
        Console.WriteLine("-- nová konverzace --");
        continue;
    }

    int checkpoint = messages.Count;
    messages.Add(new JsonObject { ["role"] = "user", ["content"] = input });
    try
    {
        var answer = fileIntent.IsMatch(input) ? await AnswerWithTools(input) : await Chat(messages);
        messages.Add(new JsonObject { ["role"] = "assistant", ["content"] = answer });
        Console.WriteLine("\nModel> " + answer);
    }
    catch (Exception ex)
    {
        Console.WriteLine("\n[Chyba] " + ex.Message);
        while (messages.Count > checkpoint) messages.RemoveAt(messages.Count - 1);
    }
}
