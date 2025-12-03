# Giai đoạn 1: Build ứng dụng
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy tệp csproj và restore packages
COPY *.csproj ./
RUN dotnet restore

# Copy toàn bộ source
COPY . ./

# Build và publish project cụ thể
RUN dotnet publish TL4_SHOP.csproj -c Release -o /app/publish

# Giai đoạn 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

EXPOSE 8080
ENTRYPOINT ["dotnet", "TL4_SHOP.dll"]
