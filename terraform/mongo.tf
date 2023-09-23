resource "null_resource" "up_mongodb" {

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
      "docker pull mongo:6.0",
      "docker run -d -e MONGO_INITDB_ROOT_USERNAME=${var.mongo_user} -e MONGO_INITDB_ROOT_PASSWORD=${var.mongo_password} -v /bot_data/mongo:/etc/mongo mongo:6.0",
      "docker container ls"
    ]
  }
}
