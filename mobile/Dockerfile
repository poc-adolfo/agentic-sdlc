# Build estático do app Ionic+Angular servido por nginx (preview web/PWA).
# Seção 9 da spec: o container só faz sentido pro preview web, não pro
# app instalável em iOS/Android (que vem via Capacitor nativo).
FROM node:20-alpine AS build
WORKDIR /app
COPY package.json .
RUN npm install
COPY . .
RUN npm run build

FROM nginx:alpine
COPY --from=build /app/dist/admin-mobile/browser /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
