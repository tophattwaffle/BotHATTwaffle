﻿// <auto-generated />
using BotHATTwaffle;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using System;

namespace BotHATTwaffle.src.Migrations
{
    [DbContext(typeof(DataBaseContext))]
    [Migration("20180329222759_Initial")]
    partial class Initial
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.0.2-rtm-10011");

            modelBuilder.Entity("BotHATTwaffle.Models.ActiveMute", b =>
                {
                    b.Property<long>("snowflake")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("mute_duration");

                    b.Property<string>("mute_reason");

                    b.Property<string>("muted_by");

                    b.Property<long>("muted_time");

                    b.Property<string>("username");

                    b.HasKey("snowflake");

                    b.ToTable("ActiveMutes");
                });

            modelBuilder.Entity("BotHATTwaffle.Models.CommandUse", b =>
                {
                    b.Property<int>("seq_id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("command");

                    b.Property<long>("date");

                    b.Property<string>("fullmessage");

                    b.Property<long>("snowflake");

                    b.Property<string>("username");

                    b.HasKey("seq_id");

                    b.ToTable("CommandUsage");
                });

            modelBuilder.Entity("BotHATTwaffle.Models.Key_Value", b =>
                {
                    b.Property<string>("key")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("value");

                    b.HasKey("key");

                    b.ToTable("KeyVaules");
                });

            modelBuilder.Entity("BotHATTwaffle.Models.Mute", b =>
                {
                    b.Property<int>("seq_id")
                        .ValueGeneratedOnAdd();

                    b.Property<long>("date");

                    b.Property<int>("mute_duration");

                    b.Property<string>("mute_reason");

                    b.Property<string>("muted_by");

                    b.Property<long>("snowflake");

                    b.Property<string>("username");

                    b.HasKey("seq_id");

                    b.ToTable("Mutes");
                });

            modelBuilder.Entity("BotHATTwaffle.Models.SearchDataResult", b =>
                {
                    b.Property<string>("name")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("url");

                    b.HasKey("name");

                    b.ToTable("SearchDataResults");
                });

            modelBuilder.Entity("BotHATTwaffle.Models.SearchDataTag", b =>
                {
                    b.Property<string>("name");

                    b.Property<string>("tag");

                    b.Property<string>("series");

                    b.HasKey("name", "tag", "series");

                    b.ToTable("SearchDataTags");
                });

            modelBuilder.Entity("BotHATTwaffle.Models.Server", b =>
                {
                    b.Property<string>("name")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("address");

                    b.Property<string>("description");

                    b.Property<string>("ftp_password");

                    b.Property<string>("ftp_path");

                    b.Property<string>("ftp_type");

                    b.Property<string>("ftp_username");

                    b.Property<string>("rcon_password");

                    b.HasKey("name");

                    b.ToTable("Servers");
                });

            modelBuilder.Entity("BotHATTwaffle.Models.Shitpost", b =>
                {
                    b.Property<int>("seq_id")
                        .ValueGeneratedOnAdd();

                    b.Property<long>("date");

                    b.Property<string>("fullmessage");

                    b.Property<string>("shitpost");

                    b.Property<long>("snowflake");

                    b.Property<string>("username");

                    b.HasKey("seq_id");

                    b.ToTable("Shitposts");
                });

            modelBuilder.Entity("BotHATTwaffle.Models.SearchDataTag", b =>
                {
                    b.HasOne("BotHATTwaffle.Models.SearchDataResult", "VirtualSearchDataResult")
                        .WithMany("tags")
                        .HasForeignKey("name")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}