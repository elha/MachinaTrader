# MyntUI - Ui for Mynt Trading Bot

This is a WIP - Dont use productive !

# Getting Started:
 * Clone repo:
   * `git clone https://github.com/LORDofDOOM/MyntUI.git`
 * Fetch Submodule:
   * `cd MyntUI`
   * `git submodule update --init --recursive`
 * Install node_modules (if you get node-sass error run `npm rebuild node-sass --force`):
   * `npm install`   
 * Build node_modules:
   * `npm run build`      
 * Restore MyntUI nuget packages:
   * `dotnet restore`
 * Run MyntUI:
   * `dotnet run`       
 * Open UI:
   * Open http://127.0.0.1:5000 in browser     

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
