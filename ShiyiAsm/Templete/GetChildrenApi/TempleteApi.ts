import { ApiBase } from "@ShiyiFramework/ShiyiApi/Base/ApiBase";
import { {{ShiyiAsm:Templete}}ReqModel,{{ShiyiAsm:Templete}}RespModel } from "./Model/{{ShiyiAsm:Templete}}ApiModel";

export class {{ShiyiAsm:Templete}}Api extends ApiBase<>{
    constructor(){
        let ApiUrl= ;
        let ReqModel=new {{ShiyiAsm:Templete}}ReqModel();
        let RespModel=new {{ShiyiAsm:Templete}}RespModel();
        super(ApiUrl,ReqModel,RespModel,(model)=>
        {
            
        })
    }
}