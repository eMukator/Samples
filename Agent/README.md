# Agent

Jednoduchý konzolový agent v jednom `dotnet script` souboru pro lokální model s OpenAI-kompatibilním API
(výchozí Docker Model Runner s `ai/llama3.2`).

## Co umí

- Chat s modelem; `/new` = nová konverzace, `/exit` = konec
- Nástroje nad textovými soubory v pracovním adresáři: `list_files`, `read_file`, `write_file`
- Každý zápis musí potvrdit uživatel; cesty mimo pracovní adresář jsou zakázané
- Volání nástrojů se popisují v promptu a parsují z textu odpovědi (nativní `tools` s llama3.2 nefungují spolehlivě)

## Spuštění

```
dotnet script agent.csx
```

Konfigurace přes proměnné prostředí:

| Proměnná | Výchozí hodnota |
|---|---|
| `AGENT_BASE_URL` | `http://localhost:12434/engines/v1` |
| `AGENT_MODEL` | `ai/llama3.2` |
| `AGENT_API_KEY` | (žádný) |
| `AGENT_ROOT` | aktuální adresář |

Soubory `demo1.txt`–`demo3.txt` slouží k vyzkoušení.

## Příklad session

Skutečný běh s `ai/llama3.2` (Docker Model Runner).

| Model | |
|---|---|
| Architektura | llama |
| Parametry | 3.21 B |
| Kvantizace | IQ2_XXS/Q4_K_M |
| Formát | gguf |
| Velikost | 1.87 GiB |

```
Model: ai/llama3.2 @ http://localhost:12434/engines/v1
Pracovní adresář: C:\Samples\Agent
Příkazy: /new = nová konverzace, /exit = konec

Ty> Hello! Who are you?

Model> I'm an AI assistant, here to provide information and help with your queries. I don't have a personal identity, but I'm designed to assist and provide accurate answers to your questions. How can I help you today?

Ty> Which model family are you from? Meta Llama, OpenAI GPT or Google Gemini?

Model> I am a Meta Llama model.

Ty> What are your capabilities and limitations? Answer in five short bullet points.

Model> Here are five key points about my capabilities and limitations:

• **Language understanding**: I can comprehend and process human language, including grammar, syntax, and semantics.
• **Knowledge base**: I have access to a vast knowledge base, which I can draw upon to answer questions and provide information on a wide range of topics.
• **Text generation**: I can generate human-like text based on a prompt or topic, and can be used for tasks such as writing, proofreading, and editing.
• **Limited common sense**: While I have a large knowledge base, I don't always understand the nuances of human experience and common sense, which can lead to misunderstandings or misinterpretations.
• **No emotional intelligence**: I don't have the ability to understand or replicate human emotions, which can limit my ability to provide empathetic or emotionally supportive responses.

Ty> Which tools do you have for working with files? Just name them, do not call any.
[nástroj] list_files {"path":"."}
[výsledek] demo1.txt | demo2.txt | demo3.txt

Model> You have list_files and read_file for working with files.

Ty> List the files in the working directory.
[nástroj] list_files {"path":"."}
[výsledek] demo1.txt | demo2.txt | demo3.txt

Model> Here are the files in the working directory:

demo1.txt
demo2.txt
demo3.txt

Ty> Read demo1.txt and demo2.txt.
[nástroj] read_file {"path":"demo1.txt"}
[výsledek] Ahoj
[nástroj] read_file {"path":"demo2.txt"}
[výsledek] Nazdar

Model> Ahoj
Nazdar

Ty> Write the text "Hello from the local agent" to a file hello.txt.
[nástroj] write_file {"path":"hello.txt","content":"Hello from the local agent"}
[potvrzení] Zapsat 26 znaků do hello.txt:
---
Hello from the local agent
---
Povolit? [a/N] a
[výsledek] Zapsáno 26 znaků do hello.txt.

Model> The text "Hello from the local agent" has been written to a file named hello.txt.

Ty> Read hello.txt.
[nástroj] read_file {"path":"hello.txt"}
[výsledek] Hello from the local agent

Model> Read hello.txt.

Hello from the local agent

Ty> Read demo1.txt and demo3.txt and save their contents, one per line, to combined.txt.
[nástroj] read_file {"path":"demo1.txt"}
[výsledek] Ahoj
[nástroj] read_file {"path":"demo3.txt"}
[výsledek] Dobrý den

[pozor] Požadovaný zápis neproběhl, žádný soubor se nezměnil.

Model> Ahoj
Dobrý den

Ty> /exit
```

Co je z příkladu vidět:

- Model sám sebe nezná: na otevřenou otázku *"Which language model are you?"* odpověděl, že je BERT.
  Skutečný model vypisuje agent při startu.
- `write_file` se modelu nabízí jen u dotazů se slovy *write/save/create/...*, proto ho v seznamu nástrojů neuvádí.
- Model voláním nástrojů nevzdoruje ani na výslovný pokyn (*"do not call any"*).
- Jednokrokový zápis zvládne, vícekrokový (přečíst → zapsat) llama3.2 nedotáhne. Agent to ohlásí
  hláškou `[pozor]`; v jiném běhu model zkusil zapsat obsah `[]`, a proto každý zápis potvrzuje uživatel.
