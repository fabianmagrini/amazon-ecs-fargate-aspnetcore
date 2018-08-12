FROM microsoft/dotnet:2.1-sdk AS build-env
WORKDIR /webapi

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# Build runtime image
FROM microsoft/dotnet:2.1-aspnetcore-runtime
WORKDIR /webapi
COPY --from=build-env /webapi/out .

#ENV ASPNETCORE_URLS http://*:6000
#EXPOSE 6000

ENTRYPOINT ["dotnet", "webapi.dll"]
