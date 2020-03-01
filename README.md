# WokLearner-Backend
Created by KanarekLife @ 2020
### How to setup?
1. Git pull https://github.com/KanarekLife/WokLearner-Backend
2. Copy `WokLearner.WebApp/sample-appsettings.json` into `WokLearner.WebApp/appsettings.json`
3. Modify `appsettings.json` file to your liking but remember that you have to provide connection string to MongoDB Database (I recommend using MongoDB Atlas)
4. Start by entering `dotnet run --urls "{here goes your ip}:{here goes your desired port}"` inside `WokLearner.WebApp` folder.
 
 (Recommend using `screen` utility on linux)

### How to update?
1. Simply enter `git pull origin master`

### How to transfer data to another server?
1. Git pull https://github.com/KanarekLife/WokLearner-Backend on new server
2. Transfer `WokLearner.WebApp/appsettings.json` and `WokLearner.WebApp/Uploads/` from old to new server
3. Run like usual!

### How to integrate?
All commands are available to see on `{ip}:{port}/swagger`!

### Todo
[X] Authentication and authorization

[X] Paintings module connected with file upload

[X] Learning module with saving progress

[ ] Completed swagger documentation

[ ] Raking system for competing purposes

[ ] Dockerfile

#### Project unfortunately was being created in a hurry so I didn't have time to design unit and integration tests. Please be careful while modifying!