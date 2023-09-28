terraform {
  cloud {
    organization = "pingvin1308"

    workspaces {
      name = "cleannetcode"
    }
  }

  required_providers {
    hcloud = {
      source  = "hetznercloud/hcloud"
      version = "~> 1.36.2"
    }

    null = {
      source  = "HashiCorp/null"
      version = "~> 3.2.1"
    }
  }
}
