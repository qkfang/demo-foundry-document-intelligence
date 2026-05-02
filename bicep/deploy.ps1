az group create --name 'rg-agentdi' --location 'australiaeast'

az deployment group create --name 'agentdi-dev' --resource-group 'rg-agentdi' --template-file './main.bicep' --parameters './main.bicepparam'
