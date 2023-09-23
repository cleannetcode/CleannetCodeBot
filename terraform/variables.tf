variable "mongo_user" {
  sensitive = true
}
variable "mongo_password" {
  sensitive = true
}
variable "hcloud_token" {
  sensitive = true
}
variable "telegram_bot_token" {
  sensitive = true
}
variable "ssh_private_key" {
  sensitive = true
}
variable "ssh_public_key" {
  sensitive = true
}
variable "image_version" {
  sensitive = true
}

