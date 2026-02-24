# ===== Build stage =====
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Solution və csproj-ları əvvəlcə kopyala (restore cache üçün)
COPY *.sln ./
COPY Core/MezuroApp.Application/MezuroApp.Application.csproj           Core/MezuroApp.Application/
COPY Core/MezuroApp.Domain/MezuroApp.Domain.csproj                     Core/MezuroApp.Domain/
COPY Infrastructure/MezuroApp.Infrastructure/MezuroApp.Infrastructure.csproj   Infrastructure/MezuroApp.Infrastructure/
COPY Infrastructure/MezuroApp.Persistance/MezuroApp.Persistance.csproj         Infrastructure/MezuroApp.Persistance/
COPY Presentation/MezuroApp.WebApi/MezuroApp.WebApi.csproj             Presentation/MezuroApp.WebApi/

# NuGet restore
RUN dotnet restore

# Bütün mənbə faylları
COPY . .

# Publish
WORKDIR /src/Presentation/MezuroApp.WebApi
RUN dotnet publish -c Release -o /app/out /p:UseAppHost=false


# ===== Runtime stage =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Əgər ffmpeg lazımdırsa saxla (yoxdursa silə bilərsən)
RUN apt-get update \
 && apt-get install -y --no-install-recommends ffmpeg \
 && rm -rf /var/lib/apt/lists/*

# Publish output
COPY --from=build /app/out .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "MezuroApp.WebApi.dll"]