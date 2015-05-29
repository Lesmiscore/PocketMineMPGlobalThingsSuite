<?php
namespace nao20010128nao;

use pocketmine\plugin\PluginBase;
use pocketmine\command\ConsoleCommandSender;
use pocketmine\event\Listener;
use pocketmine\utils\TextFormat;

class APIMain extends PluginBase implements Listener{
	private $config;
	private $cSender;
	public function onEnable(){
		$this->cSender=new ConsoleCommandSender();
		$this->cSender->sendMessage(TextFormat::GREEN."Loading config...");
		if(!file_exists($this->getDataFolder())){
			mkdir($this->getDataFolder());
		}
		if(file_exists($this->getDataFolder()."config.yml")){
			$this->config=yaml_parse_file($this->getDataFolder()."config.yml");
			if($this->config===false){
				$this->config=array("server"=>array(
						"ip"=>"nao20010128nao.dip.jp",
						"port"=>20200
					));
			}
		}else{
			$this->config=array("server"=>array(
						"ip"=>"nao20010128nao.dip.jp",
						"port"=>20200
					));
		}
		$this->cSender->sendMessage(TextFormat::GREEN."Pinging...");
	}
	public function onDisable(){
		
	}
}