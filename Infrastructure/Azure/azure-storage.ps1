az deployment group create `
  --resource-group rg-vibetravel-qa `
  --template-file azure-storage.bicep `
  --parameters `
    keyVaultName=kv-vibetravels-qa-001 `
    secretsAdminPrincipalId=1ff3ba13-1858-4638-b0df-be7379e78105