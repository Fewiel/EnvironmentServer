﻿using EnvironmentServer.Daemon.Utility;

Console.WriteLine("Hello, World!");

var template = @"version: '3.3'

services:
  db:
    image: mysql:5.7
    volumes:
    - db_data:/var/lib/mysql
    restart: always
	 ports:
	 	""$port:db:3306""
    environment:
      MYSQL_ROOT_PASSWORD: somewordpress
      MYSQL_DATABASE: wordpress
      MYSQL_USER: wordpress
      MYSQL_PASSWORD: wordpress

  wordpress:
    depends_on:
    - db
    image: wordpress:latest
    ports:
    - ""$port:wordpress:80""
    restart: always
    environment:
      WORDPRESS_DB_HOST: db:3306
      WORDPRESS_DB_USER: wordpress
      WORDPRESS_DB_PASSWORD: wordpress
volumes:
  db_data:";

var result = DockerFileBuilder.Build(template, new() { 10000, 10001, 10003 }, 10000);

Console.WriteLine();