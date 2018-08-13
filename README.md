MachinaTrader
==============

[![Build status](https://ci.appveyor.com/api/projects/status/2jcpp7x1waux011r?svg=true)](https://ci.appveyor.com/project/MachinaCore/machinatrader)

Join the Discord support Server for more up-to-date info:

[![Discord](https://discordapp.com/api/guilds/476120274459426831/widget.png)](https://discord.gg/NC5cRVp)


[Download current working branches as .paf.exe installer or .7z from appveyor](https://ci.appveyor.com/project/MachinaCore/machinatrader/build/artifacts)

# Getting Started:
 * Clone repo:
   * `git clone https://github.com/MachinaCore/MachinaTrader.git`
 * Restore MachinaTrader nuget packages:
   * `dotnet restore`
 * Run MachinaTrader:
   * `dotnet run`       
 * Open UI:
   * Open http://127.0.0.1:5000 in browser     
   
# If you need to rebuild assets (if you have e.g. changed SASS/JS files):
 * Make sure Visual Studio is closed
 * If you use Windows (if you get node-sass error run `npm rebuild node-sass --force`):
   * `MachinaTraderUIStyles.bat`   
 *  or install manually:
   * `npm install` 
   * `npm run build`  
   * `npm run build-vendors`    
   * `npm run css-compile`  
   * `npm run css-compile-vendors`   

# How to run locally with `docker` or `docker-compose`:

```bash
docker build --rm -f Dockerfile -t machinatrader:latest .
docker run --rm -d machinatrader:latest
```

or

```bash
docker-compose up -d
```

# How to run on production with `docker-compose`:

Modify `.env` environment variables:

```
# LetsEncrypt API url
ACME_CA_URI=https://acme-v01.api.letsencrypt.org/directory

# Server host name
VIRTUAL_HOST=localhost

# LetsEncrypt notifications email
LETSENCRYPT_EMAIL=me@example.com

# LetsEncrypt host name
LETSENCRYPT_HOST=example.com

# Mongo data files
MONGO_FILES_PATH=./docker/mongo

# Nginx data files
NGINX_FILES_PATH=./docker/nginx

# App data files
MACHINATRADER_FILES_PATH=./docker/machinatrader
```

Start application: 

```bash
docker-compose -f docker-compose-production.yml up -d --build
```

Stop application:

```bash
docker-compose -f docker-compose-production.yml down
```

# Recommended plugin for debugging VueJS

- Get Chrome DevTools for VueJS [here](https://chrome.google.com/webstore/detail/vuejs-devtools/nhdogjmejiglipccpnnnanhbledajbpd)
After installing the tools in chrome add these scripts to target .html file and remove or uncomment the existing vue.min.js:
```
<!-- development version, includes helpful console warnings -->
<script src="https://cdn.jsdelivr.net/npm/vue/dist/vue.js"></script>
<script>
    Vue.config.devtools = true;
</script>
```
