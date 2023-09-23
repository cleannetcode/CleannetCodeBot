# Configure the Hetzner Cloud Provider
provider "hcloud" {
  token = var.hcloud_token
}

resource "hcloud_ssh_key" "ssh_key" {
  name       = "cleannetcode"
  public_key = var.ssh_public_key
}

resource "hcloud_server" "cleannetcode_bot" {
  name        = "cleannetcode-bot"
  image       = "docker-ce"
  server_type = "cx11"
  location    = "nbg1"
  ssh_keys    = [hcloud_ssh_key.ssh_key.id]

  public_net {
    ipv4_enabled = true
    ipv6_enabled = false
  }
}