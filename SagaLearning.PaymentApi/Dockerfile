FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["SagaLearning.PaymentApi/SagaLearning.PaymentApi.csproj", "SagaLearning.PaymentApi/"]
RUN dotnet restore "SagaLearning.PaymentApi/SagaLearning.PaymentApi.csproj"
COPY . .
WORKDIR "/src/SagaLearning.PaymentApi"
RUN dotnet build "SagaLearning.PaymentApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SagaLearning.PaymentApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

CMD sleep 10s; dotnet SagaLearning.PaymentApi.dll