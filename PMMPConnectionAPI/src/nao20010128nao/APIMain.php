<?php
namespace nao20010128nao;

use pocketmine\plugin\PluginBase;
use pocketmine\command\ConsoleCommandSender;
use pocketmine\event\Listener;
use pocketmine\utils\TextFormat;
use pocketmine\event\player\PlayerJoinEvent;

class APIMain extends PluginBase implements Listener{
	private $config;
	private $cSender;
	private $connection;
	public function onEnable(){
		$this->getServer()->getPluginManager()->registerEvents($this, $this);
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
		$this->connection=new Connection($this->config,$this);
		$this->cSender->sendMessage(TextFormat::GREEN."Pinging...");
		$data=$this->connection->postData("ping",array());
		if($data===false){
			$this->cSender->sendMessage(TextFormat::RED."Failed to connect! Is the server running?");
		}else if($data==Consistants::PING_RESULT){
			$this->cSender->sendMessage(TextFormat::GREEN."Ping OK!");
		}else{
			$this->cSender->sendMessage(TextFormat::RED."Response Error! Is the server correct?");
		}
	}
	public function onDisable(){
		yaml_emit_file($this->getDataFolder()."config.yml",$this->config);
	}
	public function onPlayerJoin(PlayerJoinEvent $event){
		$player=$event->getPlayer();
		$name=$player->getName();
		$address=$player->getAddress();
		$cid=$player->getUniqueId();
	}
	public function getConnection(){
		return $this->connection;
	}
}