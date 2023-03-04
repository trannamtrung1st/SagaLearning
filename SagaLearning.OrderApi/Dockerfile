FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["SagaLearning.OrderApi/SagaLearning.OrderApi.csproj", "SagaLearning.OrderApi/"]
RUN dotnet restore "SagaLearning.OrderApi/SagaLearning.OrderApi.csproj"
COPY . .
WORKDIR "/src/SagaLearning.OrderApi"
RUN dotnet build "SagaLearning.OrderApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SagaLearning.OrderApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

CMD sleep 10s; dotnet SagaLearning.OrderApi.dll