﻿// <auto-generated />
using System;
using BotHATTwaffle;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BotHATTwaffle.src.Migrations
{
    [DbContext(typeof(DataBaseContext))]
    partial class DataBaseContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.1.4-rtm-31024");

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
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id");

                    b.Property<long?>("Duration")
                        .HasColumnName("duration");

                    b.Property<bool>("Expired")
                        .HasColumnName("expired");

                    b.Property<string>("MuterName")
                        .IsRequired()
                        .HasColumnName("muter_name");

                    b.Property<string>("Reason")
                        .HasColumnName("reason");

                    b.Property<long>("UnixTimeSeconds")
                        .HasColumnName("timestamp");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnName("user_name");

                    b.Property<long>("_userId")
                        .HasColumnName("user_id");

                    b.HasKey("Id");

                    b.HasIndex("_userId", "Expired")
                        .IsUnique()
                        .HasFilter("expired == 0");

                    b.HasIndex("_userId", "UnixTimeSeconds")
                        .IsUnique();

                    b.ToTable("mutes");
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
