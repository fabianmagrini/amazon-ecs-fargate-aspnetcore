FROM microsoft/dotnet
 
WORKDIR /webapp
COPY bin/Release/netcoreapp2.1/publish .
 
ENV ASPNETCORE_URLS http://*:5000
EXPOSE 5000
 
ENTRYPOINT ["dotnet", "webapp.dll"]