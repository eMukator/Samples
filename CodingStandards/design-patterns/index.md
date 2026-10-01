# Návrhové vzory v moderním C#

GoF a enterprise vzory tak, jak se idiomaticky píšou v .NET 8+ — většinu z nich už řeší jazyk nebo DI kontejner. Ke každému vzoru: **kdy ne**. Navazuje na `design-principles/`.

## Moduly

@pat-creational.md
@pat-structural.md
@pat-behavioral.md

## Rychlá kontrola

- [ ] Vzor řeší **existující** problém, ne očekávaný (YAGNI)
- [ ] Nejdřív zkontroluj, jestli to neřeší jazyk / framework: DI (factory, singleton), delegáty (strategy, command), `IOptions` (konfigurace), middleware / decorator (chain of responsibility)
- [ ] Singleton = registrace v DI, nikdy statická `Instance` property
- [ ] Factory jen tam, kde se rozhoduje za běhu o typu / parametrech; jinak konstruktor + DI
- [ ] Strategy přes keyed services nebo `IEnumerable<T>` + `CanHandle`, ne přes `switch` na více místech
- [ ] Decorator přes Scrutor `Decorate`, ne dědičností
- [ ] Specification jen pro opakovaně používaná doménová kritéria, jako `Expression<Func<T,bool>>`
- [ ] Název třídy nese **doménový význam**, ne jméno vzoru (`PriceCalculator`, ne `PriceStrategyFactoryImpl`)
