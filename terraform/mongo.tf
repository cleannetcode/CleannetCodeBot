locals {
  mongo_server_ip = hcloud_server.cleannetcode_bot.ipv4_address
}

resource "null_resource" "up_mongodb" {

  triggers = {
    server_ip = local.mongo_server_ip
  }

  connection {
    user        = "root"
    host        = local.mongo_server_ip
    type        = "ssh"
    private_key = var.ssh_private_key
  }

  provisioner "remote-exec" {
    inline = [
      "docker pull mongo:6.0",
      "docker run --name mongo -d -e MONGO_INITDB_ROOT_USERNAME=${var.mongo_user} -e MONGO_INITDB_ROOT_PASSWORD=${var.mongo_password} -p 27017:27017 -v /bot_data/mongo:/etc/mongo mongo:6.0 --auth",
      "docker container ls"
    ]
  }
}
