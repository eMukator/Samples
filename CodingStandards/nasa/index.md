# NASA JPL — 10 pravidel pro bezpečný kód (C# / ASP.NET Core adaptace)

Původní pravidla Gerard J. Holzmann, NASA JPL.
Tato adaptace přenáší jejich ducha do C# a JavaScriptu v kontextu ASP.NET Core.
Pravidla jsou záměrně přísná. Pokud tě neomezují, kód není dost komplexní na to, aby je bylo třeba.

## Kdy tato pravidla aplikovat

NASA pravidla jsou určena pro **kritická místa** s širokým dopadem na spolehlivost nebo výkon systému — ne pro každý řádek kódu. Aplikuj je s rozvahou tam, kde selhání nebo chyba má závažné následky.

**Typické příklady:**
- Služby běžící na pozadí (Hangfire joby, hosted services, `IHostedService`)
- Třídy zpracovávající soubory nebo média (obrázky, PDF, importy)
- Sdílené utility s vysokou četností volání
- Auth / security logika
- Integrační brány (API klienti, webhooks, messaging)

V běžném CRUD kódu, jednoduchých controllerech nebo pomocných třídách tato pravidla uplatňovat nemusíš.

## Pravidla

@rule-01-control-flow.md
@rule-02-memory.md
@rule-03-short-functions.md
@rule-04-assertions.md
@rule-05-scope.md
@rule-06-return-values.md
@rule-07-preprocessor.md
@rule-08-pointers.md
@rule-09-warnings.md
@rule-10-static-analysis.md

## Rychlá kontrola

- [ ] Žádné neomezené smyčky bez jasné horní hranice iterací
- [ ] Žádná neošetřená výjimka ani ignorovaný Task
- [ ] Funkce nepřesahuje 60 řádků
- [ ] Každá netriviální metoda má Guard assertions na vstupu
- [ ] Proměnné deklarovány co nejblíže místu použití
- [ ] Každá návratová hodnota / Result / Task je zkontrolována
- [ ] Žádné reflexe ani dynamické generování kódu bez explicitního zdůvodnění
- [ ] Žádné přímé přístupy přes více než dvě úrovně zanoření objektů
- [ ] Kód se kompiluje bez warningů (`<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`)
- [ ] Spuštěn alespoň jeden statický analyzátor (Roslyn Analyzers / SonarQube)
