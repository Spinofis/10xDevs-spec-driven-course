targetScope = 'resourceGroup'

@description('The name of the Key Vault. Must be globally unique and between 3–24 characters.')
param keyVaultName string

@description('The Azure region where the resource will be deployed.')
param location string = resourceGroup().location

@description('The Azure AD (Entra ID) tenant ID.')
param tenantId string = tenant().tenantId

@description('The object ID of the user who will manage secrets in the Key Vault.')
param secretsAdminPrincipalId string

@description('Specifies the level of public network access. Use "Disabled" when using Private Endpoints.')
param publicNetworkAccess string = 'Enabled'

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    tenantId: tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }

    enableRbacAuthorization: true

    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    publicNetworkAccess: publicNetworkAccess
  }
}

// Role: Key Vault Secrets Officer (can manage secrets)
resource secretsOfficerRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, secretsAdminPrincipalId, 'Key Vault Secrets Officer')
  scope: keyVault
  properties: {
    principalId: secretsAdminPrincipalId
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      'b86a8fe4-44ce-4948-aee5-eccb2c155cd7'
    )
  }
}

output keyVaultUri string = keyVault.properties.vaultUri
