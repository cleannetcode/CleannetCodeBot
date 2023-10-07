locals {
  mongo_connection_string = "mongodb://${var.mongo_user}:${var.mongo_password}@localhost:27017"
  bot_env = join(" ", [
    "-e TelegramBotAccessToken=${var.telegram_bot_token}",
    "-e ConnectionStrings__MongoDbConnectionString=${local.mongo_connection_string}",
  ])
}

resource "null_resource" "up_bot_container" {
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
      "docker run -d -e TelegramBotAccessToken=${var.telegram_bot_token} -e ConnectionStrings__MongoDbConnectionString=${local.mongo_connection_string} --name bot -v /bot_data/Data:/app/Data -v /bot_data/FileStorage:/app/FileStorage pingvin1308/cleannetcode.bot:${var.image_version}",
      "docker container ls"
    ]
  }
}
