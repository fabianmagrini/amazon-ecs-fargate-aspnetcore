# Reference Architecture

![](/images/aspnetcorefargate.jpg)



# Development environment
The development environment needs to have the following :-


a)Mac os 10.X or higher

b).NET core 2.1

c)Docker latest version

d)aws cli

e)aws ecs cli



# Create ASP.NET core mvc application
Let's leverage the terminal in the mac for creating, building and publishing the ASP.NET core mvc core application. Navigate to the directory where the entire set up needs to be created and issue the following commands in the terminal

mkdir webapp

cd webapp

dotnet new mvc

dotnet restore

dotnet build 

dotnet publish -c "Release"



The above set of commands creates an ASP.NET core mvc application, restores required dependencies, builds the application and publish the application package to the release folder which will be used by the ASP.NET container (to be created in the next section).



Before proceeding to create containers for ASP.NET core mvc, you can test the application locally issuing 'dotnet run' command in the terminal.



Open 'http://localhost:5000' in the browser and you should the ASP.NET  core mvc application getting rendered there.

# Containerize ASP.NET core application
Create the following Dockerfile in the mymvcweb folder.



``` Dockerfile
FROM microsoft/dotnet
 
WORKDIR /webapp
COPY bin/Release/netcoreapp2.1/publish .
 
ENV ASPNETCORE_URLS http://*:5000
EXPOSE 5000
 
ENTRYPOINT ["dotnet", "webapp.dll"]
```


The above Dockerfile definition creates an ASP.NET core 2.1 container and copies the application deployment package from 'bin/Release/netcoreapp2.1/publish' folder on to webapp folder in the container.It also leverages Kestrel as the web server and the default port of 5000 for ASP.NET core mvc application. 


# Create Nginx container

Navigate to the aspnetcorefargate directory and create a directory called 'reverseproxy'. 



``` shell
mkdir reverseproxy
cd reverseproxy
```


Create a new file called 'Nginx.conf'.



``` 
sudo nano nginx.conf


```


Edit the nginx.conf file and add the following definition.




``` conf
worker_processes 4;
 
events { worker_connections 1024; }
 
http {
    sendfile on;
 
    upstream app_servers {
        server webapp:5000;
    }
 
    server {
        listen 80;
 
        location / {
            proxy_pass         http://app_servers;
            proxy_redirect     off;
            proxy_set_header   Host $host;
            proxy_set_header   X-Real-IP $remote_addr;
            proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header   X-Forwarded-Host $server_name;
        }
    }
}

```


Since we are in the development environment we can tag service name 'webapp' for the 'upstream app_servers' section in the nginx.conf file. When this is hosted in AWS Fargate task, we need change the value of 'upstream app_servers' to '127.0.0.1:5000'. Because when the Fargate task runs in Awsvpc networking mode (which is default) it will use the local loopback interface of 127.0.01 to connect to the other container (service) defined as a part of the Fargate task which will be covered in the later sections.



Create a Dockerfile for containerizing the Nginx reverse proxy and it should look like the following:-



``` Dockerfile
FROM nginx
COPY nginx.conf /etc/nginx/nginx.conf

```



The above Docker file creates an Nginx container and copies nginx.conf file in the reverse proxy folder to the '/etc/nginx/nginx/conf' inside the containers.


# Docker compose
Now let's compose both these container as an application by defining the Docker-compose.yml. It should look like below.


``` .yaml
version: '2'
services:
  reverseproxy:
    build:
      context: ./reverseproxy
      dockerfile: Dockerfile
    ports:
      - "80:80"
    links :
      - webapp
  webapp:
    build:
      context: ./webapp
      dockerfile: Dockerfile
    ports:
      - "5000:5000"

```


The above docker-compose.yml defines two service. The first service 'webapp' relies the Dockerfile definition defined in the webapp folder and it exposes port (5000) to another service 'reverseproxy'. The second service 'reverseproxy' runs the nginx container on the port 80 and exposes port 80 to outside world. It also links with the first service 'webapp'. The links are good for docker-compose.yml in the development environment. When you convert this into ECS service definition for Fargate tasks links are not supported in the Awsvpc networking mode.



Now let's build and run these containers as a cohesive service in the local environment by issuing the following commands in the terminal.



```
docker-compose build
```


The docker-compose build should give you the following results and container ids will vary based on your environment.


```
Building webapp
Step 1/6 : FROM microsoft/dotnet
 ---> 9e243db15f91
Step 2/6 : WORKDIR /webapp
 ---> Using cache
 ---> 5f3fa4cf1f7b
Step 3/6 : COPY bin/Release/netcoreapp2.1/publish .
 ---> a5a0cfffc714
Step 4/6 : ENV ASPNETCORE_URLS http://*:5000
 ---> Running in 7f114e117695
Removing intermediate container 7f114e117695
 ---> 4b84509384c3
Step 5/6 : EXPOSE 5000
 ---> Running in c5a3da83726b
Removing intermediate container c5a3da83726b
 ---> 19a4e93b9235
Step 6/6 : ENTRYPOINT ["dotnet", "webapp.dll"]
 ---> Running in 102203e2de0b
Removing intermediate container 102203e2de0b
 ---> e1e8e8be5b6c
Successfully built e1e8e8be5b6c
Successfully tagged amazon-ecs-fargate-aspnetcore_webapp:latest
Building reverseproxy
Step 1/2 : FROM nginx
 ---> c82521676580
Step 2/2 : COPY nginx.conf /etc/nginx/nginx.conf
 ---> Using cache
 ---> bf88043f4f44
Successfully built bf88043f4f44
Successfully tagged amazon-ecs-fargate-aspnetcore_reverseproxy:latest
```


Then invoke 'docker-compose up' command in the terminal. It should give you the following results if everything is succesful.



```
Recreating amazon-ecs-fargate-aspnetcore_webapp_1 ... done
Recreating amazon-ecs-fargate-aspnetcore_reverseproxy_1 ... done
Attaching to amazon-ecs-fargate-aspnetcore_webapp_1, amazon-ecs-fargate-aspnetcore_reverseproxy_1
webapp_1        | warn: Microsoft.AspNetCore.DataProtection.KeyManagement.XmlKeyManager[35]
webapp_1        |       No XML encryptor configured. Key {ea830594-f6b3-42c9-8687-1d250442421d} may be persisted to storage in unencrypted form.
webapp_1        | Hosting environment: Production
webapp_1        | Content root path: /webapp
webapp_1        | Now listening on: http://[::]:5000
webapp_1        | Application started. Press Ctrl+C to shut down.
webapp_1        | warn: Microsoft.AspNetCore.HttpsPolicy.HttpsRedirectionMiddleware[3]
webapp_1        |       Failed to determine the https port for redirect.
```


Open http://localhost:80 in the browser and you should see the default view of index.cshtml getting rendered. After completing the testing in the local development environment you can issue the following commands to clean up docker-compose.


```
docker-compose stop
docer-compse rm
```


# Push container images to ECR
Create two ECR repositories nameyl 'webapp' and 'reverseproxy', one for the ASP.NET core mvc application and other for the nginx reverse proxy.



Now let's fetch the push commands for 'webapp' repository and execute the following in the terminal.



```
aws ecr get-login --no-include-email --region ap-southeast-2
```



It should return you the docker login command with token. Copy the Docker login with tokens and execute.

Tag the local container image (for webapp) with the remote ECR repository.



```
docker tag aspnetcorefargate_webapp:latest yourawsaccountnumber.dkr.ecr.ap-southeast-2.amazonaws.com/webapp:latest 

```



Push the 'webapp' image to the remote 'webapp' repository.



```
docker push yourawsaccountnumber.dkr.ecr.ap-southeast-2.amazonaws.com/webapp:latest 
```


Follow the same steps for 'reverseproxy' repository to push the nginx container.



```
docker tag aspnetcorefargate_reverseproxy:latest yourawsaccountnumber.dkr.ecr.ap-southeast-2.amazonaws.com/reverseproxy:latest 

docker push yourawsaccountnumber.dkr.ecr.ap-southeast-2.amazonaws.com/reverseproxy:latest 

```



# Create ECS Fargate Cluster
Let's create the ECS Fargate cluster using AWS Console.

Clusters --> Create Cluster --> Networking mode only --> Next step



![](/images/pic1.jpeg)



Name the cluster as 'aspnetcorefargatecluster' and enabled a create a new VPC for this cluster.



Leave the CIDR blocks and subnets as default and click create.



![](/images/pic2.jpeg)



You should see a successful creation of cluster and confirmation of the same.



![](/images/pic3.jpeg)



It creates a VPC with three public subnets, internet gateway and a public route table.




# Create an Application Load balancer
Navigate to the EC2 console and create an application load balancer.



Create load balancer --> Application Load Balancer and set the following :-



* Name - aspnetcorefargatealb
* Scheme - Internet facing
* IP address type - ipv4



![](/images/pic4.jpeg)



Select all the availability zones and proceed to configure security group settings.



![](/images/pic5.jpeg)



Create a new security group  'aspnetcorealbsg' for ALB.

![](/images/pic6.jpeg)



Name the target group as 'default' and set the following for the rest of the parameters.


![](/images/pic7.jpeg)



The Target type should be selected as ip address for the ECS task level load balancing.


![](/images/pic8.jpeg)



You should get a confirmation for the successful creation of the application load balancer.


![](/images/pic9.jpeg)



# Create an ECS Fargate Task
Let's leverage the aws console for creating Fargate task.

Task Definition --> Create new Task Definition --> Fargate

![](/images/pic10.jpeg)



Define the task definition name as 'aspnetcorefargatetask'  and select the appropriate task role.

![](/images/pic11.jpeg)



Select the task execution role and define the CPU and memory settings for Task size.

![](/images/pic11.jpeg)



Let's add the two containers 'webapp' and 'reverseproxy' to the container definitions.

![](/images/pic12.jpeg)



Define the container for 'reverseproxy'

![](/images/pic13.jpeg)



The container 'Links' are not supported in Fargate Task because of 'awsvpc' networking mode. Hence no need to Link containers and leave the rest of settings as default for 'reverseproxy' container.

![](/images/pic14.jpeg)



Complete the task creation and you should get the confirmation for successful creation of Fargate task.

![](/images/pic15.jpeg)




# Create an ECS Fargate Service
Let's leverage the AWS console for creating the Fargate service.

Select the 'aspnetcorecorefargatetask' --> Actions --> Create service and name it as 'aspnetcorefargatesvc'.

Specify the number of tasks as 2 and the leave the defaults for the rest.

![](/images/pic16.jpeg)



Proceed to configure network settings by selecting the appropriate VPC and subnets of the assoiated ECS Fargate cluster.

![](/images/pic17.jpeg)



Select Application Load Balancer under Load balancing.

![](/images/pic18.jpeg)



You should see 'aspnetcorefargatealb' listed there and make sure it is selected.

Under the container to load balance select the 'reverseproxy' container and click 'add to load balancer'.

![](/images/pic19.jpeg)



Select the Target name as 'default' and the rest will get auto-populated.

![](/images/pic20.jpeg)



Since Service Discovery is optional and leave it unselected. However we'll define service level autoscaling even though it is optional.

![](/images/pic21.jpeg)



Define the automatic task scaling policy like mentioned below.

![](/images/pic22.jpeg)



Proceed to complete the service creation and you should get a confirmation for successful creation of Fargate service.



![](/images/pic23.jpeg)

# Test the service

After few minutes you should see couple of tasks in running status 



![](/images/pic24.jpeg)



You should see the task IP addresses getting added to the application load balancer.



![](/images/pic25.jpeg)



The health host count in the metrics of application load balancer should also reflect two tasks.



![](/images/pic26.jpeg)



Open the DNS A record of the application load balancer in the browser.The ASP.NET core mvc application should get successfully rendered.



![](/images/pic27.jpeg)



This completes this post about hosting an ASP.NET core mvc application and nginx reverse proxy in AWS ECS Fargate.







