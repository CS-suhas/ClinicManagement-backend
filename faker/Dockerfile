FROM node:26.13.1-alpine@sha256:6bd9ab43c0ec0190ca7b2d193906cb8b7edf6e7f4afa43ddeccd6452710bffb5

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
