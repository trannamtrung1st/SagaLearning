FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["SagaLearning.WebApp/SagaLearning.WebApp.csproj", "SagaLearning.WebApp/"]
RUN dotnet restore "SagaLearning.WebApp/SagaLearning.WebApp.csproj"
COPY . .
WORKDIR "/src/SagaLearning.WebApp"
RUN dotnet build "SagaLearning.WebApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SagaLearning.WebApp.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

CMD sleep 10s; dotnet SagaLearning.WebApp.dll