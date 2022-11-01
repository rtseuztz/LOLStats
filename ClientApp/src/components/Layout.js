import React, { Component } from "react";
import { Container } from "reactstrap";
import NavMenu from "./NavMenu";

export class Layout extends Component {
  static displayName = Layout.name;

  render() {
    return (
      <div className="gradient p-3 mb-2 text-white">
        <NavMenu />
        <Container>{this.props.children}</Container>
      </div>
    );
  }
}
