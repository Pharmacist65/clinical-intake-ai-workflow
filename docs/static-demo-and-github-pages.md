# Static Demo And GitHub Pages

## Why A Browser-Only Build Exists

The repository has two frontend modes:

- **Full-stack local mode:** the React application calls the ASP.NET Core API and SQLite database.
- **Static showcase mode:** the same UI uses an in-memory TypeScript adapter with preloaded fictional cases.

Static mode lets a public-repository reviewer inspect the workflow without installing .NET, running a database, supplying API keys or entering patient information. It does not pretend to be a hosted clinical service.

## Static Mode Boundaries

The browser-only build:

- uses fictional precomputed seed cases;
- implements demo interactions in memory;
- resets changes when the page reloads;
- makes no backend API call;
- makes no external AI call;
- stores no server-side data;
- has no authentication or real integration;
- is not suitable for real patient data.

The runtime capability manifest reports these restrictions in the UI.

## Build And Preview Locally

```bash
cd frontend
npm ci
npm run build:demo
npm run preview:demo
```

Vite serves the generated HTML from `frontend/dist`. The demo build uses the repository Pages base path:

```text
/clinical-intake-ai-workflow/
```

The expected local preview URL is:

```text
http://127.0.0.1:4173/clinical-intake-ai-workflow/
```

## GitHub Pages Workflow

The repository includes `.github/workflows/pages-demo.yml`. Frontend changes on `main` run the browser-demo tests, build the static site and publish it with GitHub's official Pages actions. `workflow_dispatch` remains available for an explicit rebuild.

The stable project URL is:

[https://pharmacist65.github.io/clinical-intake-ai-workflow/](https://pharmacist65.github.io/clinical-intake-ai-workflow/)

The repository must use **GitHub Actions** as its Pages source. The first publication requires a repository administrator to enable Pages; subsequent matching `main` pushes deploy automatically to the same URL. A custom domain can be attached later in repository Pages settings without changing the application's routes or runtime boundary.

After each deployment, verify the live URL and repeat the fictional-data and network-boundary checks below.

## Publication Checklist

- CI backend tests pass.
- Standard frontend build passes.
- Static demo build passes.
- Browser console has no application errors.
- Desktop and mobile screenshots show no overlap or clipped controls.
- The Three.js workflow scene is nonblank and responsive.
- Browser network inspection shows no API, analytics or external AI request.
- Create, review queue, detail, governance and rehearsal routes work after direct navigation through the hash router.
- All visible cases and notes are clearly fictional.
- Capability manifest says external providers and live integrations are disabled.
- Repository contains no secrets, local databases, build output or local handoff notes.

## References

- [Vite static deployment guidance](https://vite.dev/guide/static-deploy.html)
- [GitHub Pages custom workflow guidance](https://docs.github.com/en/pages/getting-started-with-github-pages/using-custom-workflows-with-github-pages)
