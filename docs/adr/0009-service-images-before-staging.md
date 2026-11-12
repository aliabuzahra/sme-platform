# ADR-0009: Container images for services before Staging

**Status:** Proposed. Needs the owner (علي) before UC-013 starts.
**Date:** 2026-11-12
**Card:** UC-003 (affects UC-013)

## Context

Services run with `dotnet run` today (ADR-0005). Staging on Azure (UC-013) must run them somehow. Two paths exist.

| Option | For | Against |
| --- | --- | --- |
| A. Build an image per service, run them in Compose locally too | Local and Staging run the same artifact. Azure Container Apps or AKS deploy images directly. | Six Dockerfiles and an image build step in CI. Local inner loop gets slower unless `dotnet run` stays an option. |
| B. Deploy with `dotnet publish` to Azure App Service, no images | No Dockerfiles. Fast to set up. | Local and Staging differ. Harder to move to containers later. Less control over the runtime. |

## Decision (proposed)

Option A, with `dotnet run` kept for local development. Each service gets a `Dockerfile` based on the official `mcr.microsoft.com/dotnet/aspnet:10.0` image. CI builds the images on `main` only, not on every pull request, to stay under the 8-minute cap. Compose gains a profile that runs services from images for anyone who wants the full stack locally.

## Consequences

- UC-013 adds the Dockerfiles, the image build job and the Azure registry.
- The CI time cap may need revisiting once image builds run on `main`.
- If the owner picks B, UC-013 is smaller and this record is updated.
