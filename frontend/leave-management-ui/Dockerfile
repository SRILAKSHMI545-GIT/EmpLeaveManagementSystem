# Multi-stage Dockerfile for Angular App
FROM node:20-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .
RUN npm run build -- --configuration production

# Production Nginx Stage
FROM nginx:alpine
COPY --from=build /app/dist/leave-management-ui/browser /usr/share/nginx/html
COPY ../docker/nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
