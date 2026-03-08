# ──────────────────────────────────────────────────────────────────────────────
# Build stage
# ──────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /repo

# Restore dependencies first (leverages Docker layer cache)
COPY PrSentryAction.slnx ./
COPY src/PrSentryAction/PrSentryAction.csproj ./src/PrSentryAction/
RUN dotnet restore src/PrSentryAction/PrSentryAction.csproj

# Copy source and publish
COPY src/ ./src/
RUN dotnet publish src/PrSentryAction/PrSentryAction.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    --runtime linux-musl-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:PublishTrimmed=true

# ──────────────────────────────────────────────────────────────────────────────
# Runtime stage – minimal Alpine image
# ──────────────────────────────────────────────────────────────────────────────
FROM alpine:3.21 AS runtime
WORKDIR /action

# Install minimal CA certificates so Octokit can verify GitHub TLS
RUN apk add --no-cache ca-certificates

COPY --from=build /app/publish/PrSentryAction ./PrSentryAction

RUN chmod +x ./PrSentryAction

ENTRYPOINT ["/action/PrSentryAction"]
