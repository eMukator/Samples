# Git & CI/CD Workflow

## Moduly

@git-commits-branching.md
@git-pr-pipeline.md

## Rychlá kontrola

- [ ] Commit zpráva odpovídá Conventional Commits (`feat:`, `fix:`, `chore:`...)
- [ ] Větev vychází z aktuálního `main`, název má prefix (`feature/`, `fix/`...)
- [ ] PR šablona je vyplněna
- [ ] CI pipeline prochází (build, testy, security scan)
- [ ] Squash merge — ne merge commit
- [ ] Větev smazána po merge
- [ ] Žádné secrets v commitu
