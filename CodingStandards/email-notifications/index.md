# Email & Notifikace

## Moduly

@email-templates-sending.md
@email-deliverability.md

## Rychlá kontrola

- [ ] Email nikdy odesílán synchronně v request pipeline (Outbox pattern)
- [ ] Každý email má plain-text alternativu
- [ ] Unsubscribe link v každém marketingovém emailu
- [ ] List-Unsubscribe header nastaven
- [ ] CSS inlinováno před odesláním
- [ ] SPF, DKIM, DMARC DNS záznamy nastaveny
- [ ] Hard bounces ihned odebírány ze seznamu
- [ ] Spam complaint rate sledován (cíl < 0.1 %)
