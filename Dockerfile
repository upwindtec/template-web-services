FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /
COPY published/ ./
ENTRYPOINT ["dotnet", "WmsWebServices.dll"]
