dotnet ef migrations script `
  --idempotent `
  --project .\src\HelpDeskHero.Infrastructure `
  --startup-project .\src\HelpDeskHero.Api `
  --context AppDbContext `
  --output .\scripts\sql\migrations\HelpDeskHeroDb_InitialCreate.sql
