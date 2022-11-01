import { Summoner } from "../../d";
import Loader from "react-spinners/SyncLoader";
import "./custom.css";
export type InfoProps = {
    name: string,
    summoner: Summoner | null;
}
export default function Info(props: InfoProps) {

    var summoner = props.summoner;
    return (
        <div className="">
            <h1 id="tabelLabel">{props.name}</h1>
            {/* when the summoner data is loaded, show it */}
            {summoner ?
                <div>
                    <p>Summoner Level: {summoner.summonerLevel}</p>
                    <p>Summoner Icon: {summoner.profileIconId}</p>
                </div>
                : <Loader />
            }
        </div>
    )
}