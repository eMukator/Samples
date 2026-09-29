# API Design — REST Standards

## Moduly

@api-urls-openapi.md
@api-versioning-errors-pagination.md
@api-validation-dto.md

## Rychlá kontrola

- [ ] URL jsou podstatná jména, množné číslo, kebab-case
- [ ] Správné HTTP status kódy (201 + Location po POST, 204 po DELETE)
- [ ] Chybové odpovědi jsou RFC 9457 ProblemDetails
- [ ] Všechny endpointy jsou zdokumentovány XML komentáři
- [ ] Paginace je cursor-based s NextCursor
- [ ] FluentValidation pro vstupní validaci
- [ ] Verze API v URL prefixu
