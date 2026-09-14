ARG DOTNET_VERSION=10.0
ARG SKILL_CAP=7000

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS builder
ARG SKILL_CAP
WORKDIR /src

RUN apt-get update \
    && apt-get install -y --no-install-recommends git curl ca-certificates \
    && rm -rf /var/lib/apt/lists/*

RUN git clone https://github.com/modernuo/modernuo.git . \
    && sed -i "/m_GuildRank = RankDefinition.Lowest;/a \ \ \ \ \ \ \ \ \ \ \ \ Skills.Cap = ${SKILL_CAP};" Projects/UOContent/Mobiles/PlayerMobile.cs \
    && sed -i 's/"world.savePath", "Saves"/"world.savePath", "data\/Saves"/g' Projects/Server/World/World.cs \
    && sed -i 's/"autoArchive.archivePath", "Archives"/"autoArchive.archivePath", "data\/Archives"/g' Projects/UOContent/World\ Saves/AutoArchive.cs \
    && sed -i 's/"autoArchive.backupPath", "Backups"/"autoArchive.backupPath", "data\/Backups"/g' Projects/UOContent/World\ Saves/AutoArchive.cs \
    && chmod +x publish.sh \
    && ./publish.sh release linux x64

FROM mcr.microsoft.com/dotnet/runtime:${DOTNET_VERSION} AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends libicu-dev tzdata libdeflate0 libargon2-1 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=builder /src/Distribution /app
RUN mkdir -p /app/Data/Pathfinding && chmod -R 777 /app

EXPOSE 2593

ENTRYPOINT ["dotnet", "ModernUO.dll"]
