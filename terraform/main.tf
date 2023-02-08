variable "prefix" {
  default = "aritest"
}

variable "administrator_login" {
  default = "YoshikazuArimitsu"
}

variable "administrator_login_password" {
  default = "P@ssw0rd123!"
}

variable "db_sku" {
  default = "Basic"
}

provider "azurerm" {
  features {}
}

data "azurerm_client_config" "current" {}
data "azurerm_subscription" "current" {}
data "azuread_application_published_app_ids" "well_known" {}

resource "azurerm_resource_group" "rg" {
  location = "japaneast"
  name     = "${var.prefix}-rg"
}

# SQL server
resource "azurerm_mssql_server" "server" {
  name                = "${var.prefix}-server"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  version             = "12.0"

  azuread_administrator {
    # AzureADから管理者を追加し、SQL Server認証でのログインを禁止する
    azuread_authentication_only = true
    login_username              = "dbadmin@arimitsu2023.onmicrosoft.com"
    object_id                   = "91fc76cf-3c42-4a0d-a053-187be4fa0866"
    tenant_id                   = data.azurerm_client_config.current.tenant_id
  }
}

# SQL database
resource "azurerm_mssql_database" "db" {
  name      = "${var.prefix}-db"
  server_id = azurerm_mssql_server.server.id
  sku_name  = var.db_sku
}

# Storage
resource "azurerm_storage_account" "storage" {
  name                     = "${var.prefix}storageaccount"
  resource_group_name      = azurerm_resource_group.rg.name
  location                 = azurerm_resource_group.rg.location
  account_tier             = "Standard"
  account_replication_type = "GRS"

  # アクセスキーによるアクセスを禁止する
  # shared_access_key_enabled = false
}

resource "azurerm_storage_container" "container" {
  name                  = "container"
  storage_account_name  = azurerm_storage_account.storage.name
  container_access_type = "private"
}

# AzureAD プリンシパル
resource "azuread_service_principal" "msgraph" {
  application_id = data.azuread_application_published_app_ids.well_known.result.MicrosoftGraph
  use_existing   = true
}

resource "azuread_service_principal" "storage" {
  application_id = data.azuread_application_published_app_ids.well_known.result.AzureStorage
  use_existing   = true
}

resource "azuread_application" "mssql-app" {
  display_name     = "mssql_app"
  sign_in_audience = "AzureADMyOrg"
  owners           = [data.azurerm_client_config.current.object_id]

  required_resource_access {
    # Microsoft Graph
    resource_app_id = data.azuread_application_published_app_ids.well_known.result.MicrosoftGraph

    resource_access {
      id   = azuread_service_principal.msgraph.app_role_ids["User.Read.All"]
      type = "Scope"
    }
  }

  required_resource_access {
    # Azure Storage
    resource_app_id = data.azuread_application_published_app_ids.well_known.result.AzureStorage

    resource_access {
      id   = "03e0da56-190b-40ad-a80c-ea378c433f7f"
      type = "Scope"
    }
  }

  public_client {
    redirect_uris = [
      "https://login.microsoftonline.com/common/oauth2/nativeclient",
      "http://localhost"
    ]
  }
}

resource "azuread_service_principal" "mssql-app-sp" {
  application_id               = azuread_application.mssql-app.application_id
  app_role_assignment_required = false
  owners                       = [data.azurerm_client_config.current.object_id]
}

resource "azurerm_role_assignment" "blob_contributer" {
  scope                = data.azurerm_subscription.current.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = azuread_service_principal.mssql-app-sp.id
}

output "mssqlapp_application_id" {
  value = azuread_application.mssql-app.application_id
}

output "mssqlapp_object_id" {
  value = azuread_application.mssql-app.object_id
}
