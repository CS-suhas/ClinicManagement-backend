FROM node:22.23.5-alpine@sha256:1314fb30ec37592d5845e5a160d522c0e680972aa534549940b8d73bde88481a

WORKDIR /app
COPY package.json ./
# Patch OS packages, then drop npm/corepack once dependencies are installed:
# npm's own dependencies were most of the image scanner's findings.
RUN apk upgrade --no-cache \
 && npm install --omit=dev \
 && rm -rf /usr/local/lib/node_modules/npm /usr/local/lib/node_modules/corepack \
    /usr/local/bin/npm /usr/local/bin/npx /usr/local/bin/corepack
COPY . ./

USER node
# Same as `npm start`, which is gone with npm.
CMD ["node", "generate.js"]
