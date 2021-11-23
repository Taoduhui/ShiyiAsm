import { PesudoCompnentStack, ShiyiPageBase } from "@ShiyiFramework/ShiyiPage/Base/ShiyiPageBase";
import { {{ShiyiAsm:Templete}}Func } from "./Functional/{{ShiyiAsm:Templete}}Func";
import { {{ShiyiAsm:Templete}}Data } from "./Models/Models";
import { {{ShiyiAsm:Templete}}UI } from "./UI/{{ShiyiAsm:Templete}}UI";

interface {{ShiyiAsm:Templete}}Component extends PesudoCompnentStack{

}

export class {{ShiyiAsm:Templete}} extends ShiyiPageBase{
    public Func=new {{ShiyiAsm:Templete}}Func();
    public UI=new {{ShiyiAsm:Templete}}UI();
    public PesudoCompnents:{{ShiyiAsm:Templete}}Component={

    }
    
    public data:{{ShiyiAsm:Templete}}Data={
        Theme: "",
    }
    public onReady(){

    }
}Page(new {{ShiyiAsm:Templete}}());
