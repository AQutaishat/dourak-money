# Stage 1: build the static React bundle.
FROM node:20-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .

# Baked in at build time so the browser calls the API same-origin via Caddy's
# reverse proxy below — no CORS needed in production, unlike local dev where
# the Vite dev server (5173) and API (5080) are different origins.
ARG VITE_API_BASE_URL=/api
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL

RUN npm run build

# Stage 2: serve the built files with Caddy (also reverse-proxies /api/*).
FROM caddy:2-alpine AS runtime
COPY --from=build /app/dist /srv
COPY Caddyfile /etc/caddy/Caddyfile
