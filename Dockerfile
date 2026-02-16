FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

# Install Python for PDF processing
RUN apt-get update && apt-get install -y \
    python3 \
    python3-pip \
    && rm -rf /var/lib/apt/lists/* \
    && pip3 install --break-system-packages pdfplumber

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["BlazorApp2/BlazorApp2.csproj", "BlazorApp2/"]
RUN dotnet restore "BlazorApp2/BlazorApp2.csproj"
COPY . .
WORKDIR "/src/BlazorApp2"
RUN dotnet build "BlazorApp2.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BlazorApp2.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Copy Python processor script and Config
COPY BlazorApp2/Python /app/Python
COPY BlazorApp2/Config /app/Config

# Create directories for persistence
RUN mkdir -p /app/data /app/Config

ENV ASPNETCORE_URLS=http://+:8080
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/pdfmanager.db"
ENV OCR_CONFIG_PATH=/app/data/ocr_config.json
ENV PdfPlumber__Enabled=true
ENV PdfPlumber__PythonPath=python3
ENV PdfPlumber__ScriptPath=/app/Python/processor.py

ENTRYPOINT ["dotnet", "BlazorApp2.dll"]
