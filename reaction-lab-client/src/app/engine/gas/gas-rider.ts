import { GasAirflow } from "./gas-airflow";

export interface GasRider {
    ride(seconds: number, stepSeconds: number, airflow: GasAirflow): void;
}
