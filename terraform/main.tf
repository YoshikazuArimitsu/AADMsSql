variable "prefix" {
  default = "aritest"
}

variable "administrator_login" {
  default = "admin"
}

variable "administrator_login_password" {
  default = "P@ssw0rd123!"
}

provider "azurerm" {
  features {}
}

resource "azurerm_resource_group" "rg" {
  location = "japaneast"
  name     = "${var.prefix}-rg"
}

# sql server
resource "azurerm_sql_server" "server_primary" {
  name                         = "${var.prefix}-server"
  resource_group_name          = azurerm_resource_group.rg.name
  location                     = azurerm_resource_group.rg.location
  version                      = "12.0"
  administrator_login          = var.sqldatabase_username
  administrator_login_password = var.sqldatabase_password
}

# sql database
resource "azurerm_sql_database" "db_primary" {
  name                             = "${var.prefix}-db"
  resource_group_name              = azurerm_resource_group.rg.name
  location                         = azurerm_resource_group.rg.location
  edition                          = var.db_edition
  requested_service_objective_name = var.db_objective_name
  server_name                      = azurerm_sql_server.server_primary.name
  create_mode                      = "Default"
}
