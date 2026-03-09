---
trigger: always_on
---

Read the project documentation inside the /docs directory before generating any code.

Always follow these files:

/docs/00-global-rules.md
/docs/01-ai-governance.md
/docs/02-api-standards.md
/docs/03-architecture.md
/docs/04-database-standards.md
/docs/05-security-rules.md
/docs/06-project-domain.md

The agent must follow the documented rules for:

- API design
- response format
- architecture layers
- database standards
- security rules
- domain rules

Do not rename existing API routes, DTO fields, database columns, or project structure unless explicitly requested.

Always follow the layered architecture:

Controller -> Service -> Repository -> Database