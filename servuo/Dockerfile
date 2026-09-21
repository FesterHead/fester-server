ARG DOTNET_VERSION=10.0
ARG SERVUO_REPO=https://github.com/ServUO/ServUO.git
ARG SERVUO_COMMIT=d76bf4443cf76d081ddaf8f57c87ff33749256af

# --- Build Stage ---
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
ARG SERVUO_REPO
ARG SERVUO_COMMIT
WORKDIR /src

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
       git \
       make \
       mono-complete \
       zlib1g-dev \
       ca-certificates \
    && rm -rf /var/lib/apt/lists/*

COPY patches/ /tmp/patches/

RUN git init \
    && git remote add origin ${SERVUO_REPO} \
    && git fetch --depth 1 origin ${SERVUO_COMMIT} \
    && git checkout FETCH_HEAD \
    && git apply --verbose /tmp/patches/*.patch \
    && make build

# --- Runtime Stage ---
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS runtime
WORKDIR /server

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
       mono-complete \
       zlib1g-dev \
       libgdiplus \
       procps \
       tzdata \
    && rm -rf /var/lib/apt/lists/* \
    && ln -sf /usr/lib/x86_64-linux-gnu/libz.so.1 /usr/lib/x86_64-linux-gnu/libz.so 2>/dev/null || true \
    && ln -sf /lib/x86_64-linux-gnu/libz.so.1 /lib/x86_64-linux-gnu/libz.so 2>/dev/null || true \
    && sed -i '/<configuration>/a \        <dllmap dll="libz" target="libz.so.1" os="!windows" />' /etc/mono/config \
    && ldconfig

COPY --from=build /src /server

ENV DOTNET_CLI_HOME=/server/.dotnet \
    DOTNET_NOLOGO=true \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    HOME=/server

# Configure client path and permissions for non-root host user
RUN mkdir -p /server/Config /server/Client /server/Saves /server/Logs /server/Scripts/Custom /server/.dotnet \
    && echo "CustomPath=/server/Client" > /server/Config/DataPath.cfg \
    && chmod -R 777 /server

EXPOSE 2593/tcp 2593/udp

ENTRYPOINT ["mono", "ServUO.exe"]
