# Bezpečnost — ASP.NET Core (OWASP Top 10)

## Moduly

@sec-injection.md
@sec-headers.md
@sec-auth.md
@sec-secrets.md
@sec-rate-limiting.md
@sec-csrf-cors.md
@sec-uploads-deps.md

## Rychlá kontrola

- [ ] Žádné SQL řetězení — jen EF Core LINQ nebo FromSqlInterpolated
- [ ] Secrets v User Secrets / Key Vault, NIKDY v appsettings.json
- [ ] CSP, HSTS, X-Frame-Options, X-Content-Type-Options hlavičky
- [ ] CSRF tokeny na všech formulářích (AutoValidateAntiforgeryToken)
- [ ] CORS whitelist — nikdy AllowAnyOrigin() v produkci
- [ ] Rate limiting na auth endpointech (max 5/15min)
- [ ] @Html.Raw pouze pro sanitizovaný nebo vlastní obsah
- [ ] Upload: magic bytes ověření, nový název, mimo wwwroot
- [ ] dotnet list package --vulnerable v CI pipeline
- [ ] Závislosti bez known CVE (Dependabot aktivní)
