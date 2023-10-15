locals {
  mongo_connection_string = "'mongodb://${var.mongo_user}:${urlencode(var.mongo_password)}@172.17.0.2:27017'"

}

resource "null_resource" "up_telegram_bot_container" {
  triggers = {
    build_number = timestamp()
  }

  connection {
    user        = "root"
    host        = hcloud_server.cleannetcode_bot.ipv4_address
    type        = "ssh"
    private_key = var.ssh_private_key
  }

  provisioner "remote-exec" {
    inline = [
      "docker container stop bot &> /dev/null",
      "docker container rm bot &> /dev/null",
      "docker rmi $(docker images | grep 'cleannetcode.bot') &> /dev/null",
      "docker pull pingvin1308/cleannetcode.bot:${var.image_version}",
      "docker run -d -e TelegramBotAccessToken=${var.telegram_bot_token} -e ConnectionStrings__MongoDbConnectionString=${local.mongo_connection_string} --name bot -v /bot_data/Data:/app/Data -v /bot_data/FileStorage:/app/FileStorage pingvin1308/cleannetcode.bot:${var.image_version}",
      "docker container ls"
    ]
  }
}
