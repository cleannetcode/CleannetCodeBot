locals {
  bot_env = join(" ", [
    "-e TelegramBotAccessToken=${var.telegram_bot_token}",
    "-e ConnectionStrings__MongoDbConnectionString=\"mongodb://${var.mongo_user}:${var.mongo_password}@mongo:27017\""
  ])
}

resource "null_resource" "up_bot_container" {
  triggers = {
    build_number = timestamp()
  }
  depends_on = [
    null_resource.up_mongodb
  ]

  connection {
    user        = "root"
    host        = hcloud_server.cleannetcode_bot.ipv4_address
    type        = "ssh"
    private_key = var.ssh_private_key
  }

  provisioner "remote-exec" {
    inline = [
      "docker rm -f $(docker ps -a -q)",
      "docker pull pingvin1308/cleannetcode.bot:${var.image_version}",
      "docker run -d ${local.bot_env} -v /bot_data/Data:/app/Data -v /bot_data/FileStorage:/app/FileStorage pingvin1308/cleannetcode.bot:${var.image_version}",
      "docker container ls"
    ]
  }
}
