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
		
	}
	private function generateUrl($dir,array $values){
		$url="http://".$this->config["server"]["ip"].":".$this->config["server"]["port"]."/".$dir;
		$keys=array_keys($values);
		return $url;
	}
}