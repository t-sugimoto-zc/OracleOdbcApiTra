# --- Build Stage ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# C# プロジェクトをコピーしてビルド
COPY ./OracleOdbcApi ./OracleOdbcApi
WORKDIR /src/OracleOdbcApi
RUN dotnet publish -c Release -o /app/publish

# --- Runtime Stage ---
FROM ubuntu:20.04

# 必要なリポジトリの追加
RUN echo "deb http://archive.ubuntu.com/ubuntu focal main universe" > /etc/apt/sources.list

# 必要なパッケージのインストール
RUN apt-get update

RUN apt-get install -y libaio1
RUN apt-get install -y wget
RUN apt-get install -y unzip
RUN apt-get install -y unixodbc
RUN apt-get install -y curl
RUN apt-get install -y gpg

# Oracle Instant Clientのインストール
COPY instantclient-basic-linux.x64-19.26.0.0.0dbru.zip /opt/oracle/
COPY instantclient-odbc-linux.x64-19.26.0.0.0dbru.zip /opt/oracle/
RUN cd /opt/oracle
RUN unzip -o /opt/oracle/instantclient-basic-linux.x64-19.26.0.0.0dbru.zip -d /opt/oracle
RUN unzip -o /opt/oracle/instantclient-odbc-linux.x64-19.26.0.0.0dbru.zip -d /opt/oracle
RUN rm -f /opt/oracle/*.zip
RUN ln -s /opt/oracle/instantclient_19_26 /opt/oracle/instantclient
RUN ls -a /opt/oracle/instantclient
RUN echo /opt/oracle/instantclient > /etc/ld.so.conf.d/oracle-instantclient.conf
RUN ldconfig

# TNS_ADMIN ディレクトリの作成と環境変数の設定
RUN mkdir -p /opt/oracle/instantclient/network/admin
ENV TNS_ADMIN=/opt/oracle/instantclient/network/admin

# ユーザデータソース用ディレクトリの作成と環境変数の設定
RUN mkdir -p /home/ubuntu
ENV HOME=/home/ubuntu

ENV ODBCINI=/etc/odbc.ini

ENV LD_LIBRARY_PATH=/opt/oracle/instantclient:$LD_LIBRARY_PATH

# iniファイルとoraファイルの作成
COPY odbc.ini /etc/
COPY .odbc.ini /home/ubuntu/
COPY odbcinst.ini /etc/
COPY tnsnames.ora /opt/oracle/instantclient/network/admin/

# ドライバーの登録
RUN odbcinst -i -d -f /etc/odbcinst.ini
RUN odbcinst -q -d

# .NET ランタイムのインストール
RUN wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
RUN dpkg -i packages-microsoft-prod.deb
RUN apt-get update
RUN apt-get install -y aspnetcore-runtime-8.0
RUN rm packages-microsoft-prod.deb

# アプリのコピーと起動
COPY --from=build /app/publish /app
WORKDIR /app
ENTRYPOINT ["dotnet", "OracleOdbcApi.dll"]

ENV ASPNETCORE_URLS=http://0.0.0.0:5000

