## Run eClaimApp, eClaimApi, SQL Server, and Redis in Docker container 

### SQL Server image  

#### 1. Pull MSSQL image
    $ docker pull mcr.microsoft.com/mssql/server:2022-latest  

#### 2. Run MSSQL in container
    $ docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=Amit@123" -p 1433:1433 --name sqlserver -d mcr.microsoft.com/mssql/server:2022-latest

#### 3. Check SQL Server running status
    $ docker ps 

    Output :
    CONTAINER ID        IMAGE                                           COMMAND                     CREATED          STATUS                    PORTS                                            NAMES
    6ed4682938bb        mcr.microsoft.com/mssql/server:2022-latest      "/opt/mssql/bin/laun…"      14 minutes ago   Up 14 minutes             0.0.0.0:1433->1433/tcp, [::]:1433->1433/tcp      sqlserver

#### 4. Check Hostname for connect SQL server
    $ hostname -I

#### 5. SQL connection details
    Server: 172.17.221.91,1433
    Login: SA
    Password: Amit@123    


### Redis image   

#### 1. Pull Redis image
    $ docker pull redis

#### 2. Run Redis in container
    $ docker run -d --name redis-server -p 6379:6379 -v redis_data:/data redis redis-server --appendonly yes

    Comment:
	-d → Detached mode (runs in background)
	--name redis-server → Names the container
	-p 6379:6379 → Maps Redis port 6379 to localhost:6379
	redis → The image name
    -v redis_data:/data → Stores Redis data in a Docker volume
	--appendonly yes → Enables persistence

#### 3. Check to Redis connection
    d$ ocker exec -it redis-server redis-cli

    Try for testing:
		ping
	Response should be:
		PONG

## Run backend Application in container

#### 1. Pull app image
    git clone https://github.com/amitkumaramithotmailcom/E-Claim-Docker.git

#### 2. Move on dockerfile folder
    $ cd E-Claim-Docker
    E-Claim-Docker$ cd E-Claim-Service
    
#### 3. Run docker build command for docker image
    $ docker build -t amitkumaramit/eclaim_api -f EClaim.API/Dockerfile .

    Comment:
    amitkumaramithotmailcom/e-claim-docker : Image Name
    EClaim.API/Dockerfile : docker file path

    Dockerfile:
        # Base runtime image
        FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
        WORKDIR /app
        EXPOSE 8080

        # Configure Kestrel to listen on port 8080
        ENV ASPNETCORE_URLS=http://+:8080

        # Build stage
        FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
        ARG BUILD_CONFIGURATION=Release
        WORKDIR /src

        # Copy only the csproj and restore (better caching)
        COPY ["EClaim.API/EClaim.API.csproj", "EClaim.API/"]
        RUN dotnet restore "EClaim.API/EClaim.API.csproj"

        # Copy everything else
        COPY . .

        # Build
        WORKDIR /src/EClaim.API
        RUN dotnet build "EClaim.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

        # Publish stage
        FROM build AS publish
        ARG BUILD_CONFIGURATION=Release
        RUN dotnet publish "EClaim.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

        # Final runtime image
        FROM base AS final
        WORKDIR /app
        COPY --from=publish /app/publish .
        ENTRYPOINT ["dotnet", "EClaim.API.dll"]

#### 4. Run backedn application
    $ docker run -d -p 5000:5000 -e ASPNETCORE_URLS=http://+:5000 amitkumaramit/eclaim_api

#### 5. Check backend app running status
    $ docker ps 

    Output :
    CONTAINER ID        IMAGE                           COMMAND                     CREATED          STATUS                    PORTS                                                                                        NAMES
    6b380631a8c0        amitkumaramit/eclaim_api        "dotnet EClaim.API.d…"      12 minutes ago   Up 12 minutes             8080/tcp, 0.0.0.0:5000->80/tcp, 0.0.0.0:5001->80/tcp, [::]:5000->80/tcp, [::]:5001->80/tcp   eclaim_api

#### 6. App accessable on below port
    http://172.17.221.91:5000/
    http://172.17.221.91:5001/
    http://172.17.221.91:5000/swagger/index.html
