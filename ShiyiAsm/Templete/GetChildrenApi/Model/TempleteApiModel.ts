import { RequestModelBase } from "@ShiyiFramework/ShiyiApi/Base/RequestModelBase";
import { ResponseDataModelBase, ResponseModelBase } from "@ShiyiFramework/ShiyiApi/Base/ResponseModelBase";

export class {{ShiyiAsm:Templete}}ReqModel extends RequestModelBase {
    constructor() {
        super();
    }
    public GetMethod(): "OPTIONS" | "GET" | "HEAD" | "POST" | "PUT" | "DELETE" | "TRACE" | "CONNECT" | undefined {
        return ;
}
    public GetBody(): Record<string, any> {
        return ;
    }

}
export interface {{ShiyiAsm:Templete}}ApiRespData extends ResponseDataModelBase{

}
 
export class {{ShiyiAsm:Templete}}RespModel extends ResponseModelBase<> {

    public Parse(data: {{ShiyiAsm:Templete}}ApiRespData) {
        //TODO: Parse Method
        return data.data;;
    }
    
}