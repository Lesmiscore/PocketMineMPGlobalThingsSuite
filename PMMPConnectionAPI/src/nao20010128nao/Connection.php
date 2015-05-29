<?php
namespace nao20010128nao;

class Connection{
	private $config;
	private $main;
	public __construct(array $config,APIMain $main){
		$this->config=$config;
		$this->main=$main;
	}
	public function postData($dir,array $values){
		$url=$this->generateUrl($dir,$values);
		$resp=file($url);
		if($resp===false){
			return false;
		}
		return rtrim($resp[0]);
	}
	private function generateUrl($dir,array $values){
		$url="http://".$this->config["server"]["ip"].":".$this->config["server"]["port"]."/?".$dir;
		$keys=array_keys($values);
		foreach($keys as $key){
			$url=$url.$key."=".$values[$key]."&";
		}
		$url=substr($url,0,strlen($url)-1);
		return $url;
	}
}